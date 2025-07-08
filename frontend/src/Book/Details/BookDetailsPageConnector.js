import { push } from 'connected-react-router';
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import NotFound from 'Components/NotFound';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { fetchBookBySlug } from 'Store/Actions/bookActions';
import translate from 'Utilities/String/translate';
import BookDetailsConnector from './BookDetailsConnector';

function createMapStateToProps() {
  return createSelector(
    (state, { match }) => match,
    (state) => state.books,
    (state) => state.authors,
    (match, books, author) => {
      const titleSlug = match.params.titleSlug;
      const isFetching = books.isFetching || author.isFetching;
      const isPopulated = books.isPopulated && author.isPopulated;

      if (!isFetching && isPopulated) {
        const bookIndex = _.findIndex(books.items, { titleSlug });
        if (bookIndex === -1) {
          return {
            books,
            isFetching,
            isPopulated
          };
        }
      }

      return {
        titleSlug,
        books,
        isFetching,
        isPopulated
      };
    }
  );
}

const mapDispatchToProps = {
  push,
  fetchBookBySlug
};

class BookDetailsPageConnector extends Component {

  constructor(props) {
    super(props);
    this.state = { hasMounted: false };
  }
  //
  // Lifecycle

  componentDidMount() {
    this.populate();
    this.fetchBookIfNeeded();
  }

  componentDidUpdate(prevProps) {
    if (prevProps.titleSlug !== this.props.titleSlug && this.props.titleSlug) {
      this.fetchBookIfNeeded();
    }
  }

  //
  // Control

  populate = () => {
    this.setState({ hasMounted: true });
  };

  fetchBookIfNeeded = () => {
    const { titleSlug, books, fetchBookBySlug: fetchBookBySlugProp } = this.props;
    const book = books && books.items && books.items.find((b) => b.titleSlug === titleSlug);
    if (!book && books && !books.isFetching) {
      fetchBookBySlugProp({ titleSlug });
    }
  };

  //
  // Render

  render() {
    const {
      titleSlug,
      isFetching,
      isPopulated
    } = this.props;

    if (!titleSlug) {
      return (
        <NotFound
          message={translate('SorryThatBookCannotBeFound')}
        />
      );
    }

    if ((isFetching || !this.state.hasMounted) ||
        (!isFetching && !isPopulated)) {
      return (
        <PageContent title={translate('Loading')}>
          <PageContentBody>
            <LoadingIndicator />
          </PageContentBody>
        </PageContent>
      );
    }

    if (!isFetching && isPopulated && this.state.hasMounted) {
      return (
        <BookDetailsConnector
          titleSlug={titleSlug}
        />
      );
    }
  }
}

BookDetailsPageConnector.propTypes = {
  titleSlug: PropTypes.string,
  books: PropTypes.object,
  match: PropTypes.shape({ params: PropTypes.shape({ titleSlug: PropTypes.string.isRequired }).isRequired }).isRequired,
  push: PropTypes.func.isRequired,
  fetchBookBySlug: PropTypes.func.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookDetailsPageConnector);
