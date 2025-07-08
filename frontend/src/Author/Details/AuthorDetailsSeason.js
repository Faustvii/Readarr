import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import TablePager from 'Components/Table/TablePager';
import { sortDirections } from 'Helpers/Props';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import getToggledRange from 'Utilities/Table/getToggledRange';
import BookRowConnector from './BookRowConnector';
import styles from './AuthorDetailsSeason.css';

class AuthorDetailsSeason extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      lastToggledBook: null
    };
  }

  componentDidMount() {
    this.props.setSelectedState(this.props.items);
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection,
      setSelectedState
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      setSelectedState(items);
    }
  }

  //
  // Listeners

  onMonitorBookPress = (bookId, monitored, { shiftKey }) => {
    const lastToggled = this.state.lastToggledBook;
    const bookIds = [bookId];

    if (shiftKey && lastToggled) {
      const { lower, upper } = getToggledRange(this.props.items, bookId, lastToggled);
      const items = this.props.items;

      for (let i = lower; i < upper; i++) {
        bookIds.push(items[i].id);
      }
    }

    this.setState({ lastToggledBook: bookId });

    this.props.onMonitorBookPress(_.uniq(bookIds), monitored);
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    const {
      onSelectedChange,
      items
    } = this.props;

    return onSelectedChange(items, id, value, shiftKey);
  };

  //
  // Render

  render() {
    const {
      items,
      isEditorActive,
      columns,
      sortKey,
      sortDirection,
      onSortPress,
      onTableOptionChange,
      selectedState,
      page,
      totalPages,
      totalRecords,
      isFetching,
      onFirstPagePress,
      onPreviousPagePress,
      onNextPagePress,
      onLastPagePress,
      onPageSelect,
      pageSize
    } = this.props;

    let titleColumns = columns;
    if (!isEditorActive) {
      titleColumns = columns.filter((x) => x.name !== 'select');
    }

    const tableOptionsProps = {
      ...this.props,
      pageSize,
      onTableOptionChange
    };

    return (
      <div
        className={styles.bookType}
      >
        <div className={styles.books}>
          <TableOptionsModalWrapper
            {...tableOptionsProps}
            columns={columns}
          >
            <Table
              columns={titleColumns}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
              onTableOptionChange={onTableOptionChange}
              pageSize={pageSize}
            >
              <TableBody>
                {
                  items.map((item) => {
                    return (
                      <BookRowConnector
                        key={item.id}
                        columns={columns}
                        {...item}
                        onMonitorBookPress={this.onMonitorBookPress}
                        isEditorActive={isEditorActive}
                        isSelected={selectedState[item.id]}
                        onSelectedChange={this.onSelectedChange}
                      />
                    );
                  })
                }
              </TableBody>
            </Table>
          </TableOptionsModalWrapper>

          {
            totalPages > 1 &&
              <TablePager
                page={page}
                totalPages={totalPages}
                totalRecords={totalRecords}
                isFetching={isFetching}
                onFirstPagePress={onFirstPagePress}
                onPreviousPagePress={onPreviousPagePress}
                onNextPagePress={onNextPagePress}
                onLastPagePress={onLastPagePress}
                onPageSelect={onPageSelect}
              />
          }
        </div>
      </div>
    );
  }
}

AuthorDetailsSeason.propTypes = {
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  selectedState: PropTypes.object.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired,
  setSelectedState: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onMonitorBookPress: PropTypes.func.isRequired,
  uiSettings: PropTypes.object.isRequired,
  page: PropTypes.number,
  totalPages: PropTypes.number,
  totalRecords: PropTypes.number,
  isFetching: PropTypes.bool,
  onFirstPagePress: PropTypes.func,
  onPreviousPagePress: PropTypes.func,
  onNextPagePress: PropTypes.func,
  onLastPagePress: PropTypes.func,
  onPageSelect: PropTypes.func,
  pageSize: PropTypes.number
};

AuthorDetailsSeason.defaultProps = {
  page: 1,
  totalPages: 1,
  totalRecords: 0,
  isFetching: false
};

export default AuthorDetailsSeason;
